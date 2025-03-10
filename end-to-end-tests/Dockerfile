#
#  EndToEndTest - base
#
FROM bash AS tests-base
COPY --from=fullstorydev/grpcurl /bin/grpcurl /bin/grpcurl
WORKDIR /work
COPY e2e_tests.sh .

#
#  EndToEndTest - tests-run
#
FROM tests-base AS end-to-end-test
# Default command
CMD ["bash", "./e2e_tests.sh","yarpgrpc-proxy:8080"]
